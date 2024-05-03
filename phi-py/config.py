class Config:
    SECRET_KEY = 'your_secret_key_here'
    DATABASE_URI = 'your_database_uri_here'
    DEBUG = True
    PORT = 9090

class ProductionConfig(Config):
    DEBUG = False

class DevelopmentConfig(Config):
    DEBUG = True
    DATABASE_URI = 'your_dev_database_uri_here'

class TestingConfig(Config):
    TESTING = True
    DATABASE_URI = 'your_test_database_uri_here'