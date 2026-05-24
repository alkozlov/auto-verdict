After navigating to target url for otomoto.pl site the parsing proceure should be performed:

1. Accept cookeis: if cookies overlay appeared then click on button with id "onetrust-pc-btn-handler"; wait for dialog appeared and click on button with id "ot-pc-refuse-all-handler"
2. Extract following information from the page:
- car make and model: extract text from h1 element with class "offer-title big text". Mandatory field
- price: extract text from h3 element with class "offer-price__number". Mandatory field

Find out div block with attribute data-testid="main-details-section". This block contains multiple div blocks with additional information. Each of them has the same structure: 2 <p> elements. The first one contains caracteristic name, the second one contains its value. Extract all of them and organize in a dictionary where key is caracteristic name and value is its value. For example:
{
  "Rok produkcji": "2015",
  "Przebieg": "100 000 km",
  ...
}

Find out a dic block with attribute data-testid="content-description-section". It contains several div block inside. Our target is the second div block. It contains description of an auto. First of all we should check if there is button tag inside the block. If there is button tag then we should click on it to expand the description. After that we can extract text from the block and save it as description of an auto. To do that we should get div tag with attribute data-testid="textWrapper" and extract text from it. It could contains another div tag and p tag. We need to extract the text in a way that preserves as much formatting as possible. Extracted description should be stored to te same dictionary variable with key "Description" and value is the extracted description.

The next batch of data should be extracted from the div block with attribute data-testid="basic_information". It contains multiple div blocks with class "flex place-items-center". We should iterate them and extract key-value pairs:
- key is the text from p tag with class "text-foreground-secondary"
- value is the text from p tag with class "font-normal"

Exclude 2 key-value pairs with keys "VIN" and "Kup ten pojazd na raty" because they are not relevant for our task.

All extracted key-value pairs should be stored in the same dictionary variable. For example:
{
  "Rok produkcji": "2015",
  "Przebieg": "100 000 km",
  "Pojemność skokowa": "2000 cm3",
  ...
}

